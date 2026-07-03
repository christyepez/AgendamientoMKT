#!/usr/bin/env bash
set -euo pipefail

deploy_path="${1:?Deployment path is required}"
environment="${2:?Environment name is required}"
branch="${3:?Branch name is required}"

case "$branch" in
  dev|test|prod) ;;
  *) echo "Unsupported branch: $branch" >&2; exit 1 ;;
esac

cd "$deploy_path"
git fetch origin "$branch"
git checkout "$branch"
git pull --ff-only origin "$branch"

compose_file="deploy/docker-compose.${environment}.yml"
if [[ ! -f "$compose_file" ]]; then
  compose_file="docker-compose.yml"
fi

if [[ ! -f "$compose_file" ]]; then
  echo "No Compose file found for $environment." >&2
  exit 1
fi

docker compose -f "$compose_file" pull
docker compose -f "$compose_file" up -d --remove-orphans
docker compose -f "$compose_file" ps

