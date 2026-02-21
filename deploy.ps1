# Production deployment helper script
# Usage: .\deploy.ps1 up -d --build
#        .\deploy.ps1 logs -f
#        .\deploy.ps1 down

docker compose -f docker-compose.yml -f docker-compose.prod.yml @args
