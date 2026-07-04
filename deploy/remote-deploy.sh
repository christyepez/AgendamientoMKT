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
[[ -f "$compose_file" ]] || compose_file="deploy/docker-compose.remote.yml"

if [[ ! -f "$compose_file" ]]; then
  echo "No Compose file found for $environment." >&2
  exit 1
fi

env_file="deploy/.env.${environment}"
if [[ ! -f "$env_file" ]]; then
  echo "Missing environment file: $env_file" >&2
  exit 1
fi

export IMAGE_TAG="$branch"
docker compose --project-name "agendamiento-mkt-${environment}" --env-file "$env_file" -f "$compose_file" pull
docker compose --project-name "agendamiento-mkt-${environment}" --env-file "$env_file" -f "$compose_file" up -d --remove-orphans
docker compose --project-name "agendamiento-mkt-${environment}" --env-file "$env_file" -f "$compose_file" ps
