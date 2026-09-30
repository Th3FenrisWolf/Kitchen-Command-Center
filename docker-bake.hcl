variable "KCC_REGISTRY" {
  default = "ghcr.io/th3fenriswolf"
}

variable "KCC_IMAGE_TAG" {
  default = "local"
}

group "default" {
  targets = ["app", "ssr", "backup"]
}

target "_image" {
  platforms = ["linux/arm64"]
  labels = {
    "org.opencontainers.image.source" = "https://github.com/Th3FenrisWolf/Kitchen-Command-Center"
  }
  attest = ["type=provenance,disabled=true", "type=sbom,disabled=true"]
}

target "_site" {
  inherits = ["_image"]
  context  = "."
  secret   = ["id=fontawesome,env=FONTAWESOME_NPM_AUTH_TOKEN"]
}

target "app" {
  inherits = ["_site"]
  target   = "app"
  tags     = ["${KCC_REGISTRY}/kcc-app:${KCC_IMAGE_TAG}"]
}

target "ssr" {
  inherits = ["_site"]
  target   = "ssr"
  tags     = ["${KCC_REGISTRY}/kcc-ssr:${KCC_IMAGE_TAG}"]
}

target "backup" {
  inherits = ["_image"]
  context  = "deploy/kcc-backup"
  tags     = ["${KCC_REGISTRY}/kcc-backup:${KCC_IMAGE_TAG}"]
}
