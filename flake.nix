{
  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixpkgs-unstable";
    flake-utils = {
      url = "github:numtide/flake-utils";
    };
  };

  outputs = {
    self,
    nixpkgs,
    flake-utils,
    ...
  }:
    flake-utils.lib.eachDefaultSystem (system: let
      pkgs = nixpkgs.legacyPackages.${system};
      deps = builtins.fromJSON (builtins.readFile ./nix/deps.json);
      projectFile = "./AnkiStatic/AnkiStatic.fsproj";
      testProjectFile = "./AnkiStatic.Test/AnkiStatic.Test.fsproj";
      pname = "anki-static";
      dotnet-sdk = pkgs.dotnet-sdk_8;
      dotnet-runtime = pkgs.dotnetCorePackages.runtime_8_0;
      version = "0.1";
      dotnetTool = dllOverride: toolName: toolVersion: hash:
        pkgs.stdenvNoCC.mkDerivation rec {
          name = toolName;
          version = toolVersion;
          nativeBuildInputs = [pkgs.makeWrapper];
          src = pkgs.fetchNuGet {
            pname = name;
            version = version;
            hash = hash;
            installPhase = ''mkdir -p $out/bin && cp -r tools/net*/any/* $out/bin'';
          };
          installPhase = let
            dll =
              if isNull dllOverride
              then name
              else dllOverride;
          in
            # fsharp-analyzers requires the .NET SDK at runtime, so we use that instead of dotnet-runtime.
            ''
              runHook preInstall
              mkdir -p "$out/lib"
              cp -r ./bin/* "$out/lib"
              makeWrapper "${dotnet-sdk}/bin/dotnet" "$out/bin/${name}" --set DOTNET_HOST_PATH "${dotnet-sdk}/bin/dotnet" --add-flags "$out/lib/${dll}.dll"
              runHook postInstall
            '';
        };
        fantomas = dotnetTool null "fantomas" (builtins.fromJSON (builtins.readFile ./.config/dotnet-tools.json)).tools.fantomas.version (builtins.head (builtins.filter (elem: elem.pname == "fantomas") deps)).hash;
    in {
      packages = {
        fantomas = fantomas;
        default = pkgs.buildDotnetModule {
          inherit pname version projectFile testProjectFile dotnet-sdk dotnet-runtime;
          name = "anki-static";
          src = ./.;
          nugetDeps = ./nix/deps.json; # `nix build .#default.fetch-deps && ./result nix/deps.json && rm result`
          doCheck = true;
        };
      };
      apps = {
        default = {
          type = "app";
          program = "${self.packages.${system}.default}/bin/AnkiStatic";
        };
      };
      devShells.default = pkgs.mkShell {
        buildInputs = [pkgs.alejandra dotnet-sdk pkgs.python3];
      };
      checks = {
        fantomas = pkgs.stdenvNoCC.mkDerivation {
          name = "fantomas-check";
          src = ./.;
          checkPhase = ''
            ${fantomas}/bin/fantomas --check .
          '';
          installPhase = "mkdir $out";
          dontBuild = true;
          doCheck = true;
        };
        verify = pkgs.stdenvNoCC.mkDerivation {
          name = "verify-schema";
          src = ./.;
          checkPhase = ''
            ${self.packages.${system}.default}/bin/AnkiStatic verify AnkiStatic.Test/CapitalsOfTheWorld.json
          '';
          installPhase = "mkdir $out";
          dontBuild = true;
          doCheck = true;
        };
      };
    });
}
