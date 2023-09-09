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
      pname = "gitea-repo-config";
      dotnet-sdk = pkgs.dotnet-sdk_7;
      dotnet-runtime = pkgs.dotnetCorePackages.runtime_7_0;
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
    in {
      packages = {
        fantomas = dotnetTool "fantomas" (builtins.fromJSON (builtins.readFile ./.config/dotnet-tools.json)).tools.fantomas.version "sha256-83RodORaC3rkYfbFMHsYLEtl0+8+akZXcKoSJdgwuUo=";
      };
      devShells.default = pkgs.mkShell {
        buildInputs =
          [pkgs.alejandra pkgs.dotnet-sdk_7 pkgs.python3]
          ++ (
            if pkgs.stdenv.isDarwin
            then [pkgs.darwin.apple_sdk.frameworks.CoreServices]
            else []
          );
      };
    });
}
