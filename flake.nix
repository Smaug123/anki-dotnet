{
  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs";
    flake-utils = {
      url = "github:numtide/flake-utils";
    };
  };

  outputs = inputs @ {
    self,
    nixpkgs,
    flake-utils,
    ...
  }:
    flake-utils.lib.eachDefaultSystem (system: let
      pkgs = nixpkgs.legacyPackages.${system};
      projectFile = "./AnkiStatic/AnkiStatic.fsproj";
      testProjectFile = "./AnkiStatic.Test/AnkiStatic.Test.fsproj";
      pname = "anki-static";
      dotnet-sdk = pkgs.dotnet-sdk_8;
      dotnet-runtime = pkgs.dotnetCorePackages.runtime_8_0;
      version = "0.1";
      dotnetTool = toolName: toolVersion: sha256:
        pkgs.stdenvNoCC.mkDerivation rec {
          name = toolName;
          version = toolVersion;
          nativeBuildInputs = [pkgs.makeWrapper];
          src = pkgs.fetchNuGet {
            pname = name;
            version = version;
            sha256 = sha256;
            installPhase = ''mkdir -p $out/bin && cp -r tools/net6.0/any/* $out/bin'';
          };
          installPhase = ''
            runHook preInstall
            mkdir -p "$out/lib"
            cp -r ./bin/* "$out/lib"
            makeWrapper "${dotnet-runtime}/bin/dotnet" "$out/bin/${name}" --add-flags "$out/lib/${name}.dll"
            runHook postInstall
          '';
        };
      fantomas = dotnetTool "fantomas" (builtins.fromJSON (builtins.readFile ./.config/dotnet-tools.json)).tools.fantomas.version (builtins.head (builtins.filter (elem: elem.pname == "fantomas") ((import ./nix/deps.nix) {fetchNuGet = x: x;}))).sha256;
    in {
      packages = {
        fantomas = fantomas;
        default = pkgs.buildDotnetModule {
          inherit pname version projectFile testProjectFile dotnet-sdk dotnet-runtime;
          name = "anki-static";
          src = ./.;
          nugetDeps = ./nix/deps.nix; # `nix build .#default.passthru.fetch-deps && ./result` and put the result here
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
        buildInputs =
          [pkgs.alejandra dotnet-sdk pkgs.python3]
          ++ (
            if pkgs.stdenv.isDarwin
            then [pkgs.darwin.apple_sdk.frameworks.CoreServices]
            else []
          );
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
