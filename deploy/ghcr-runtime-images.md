# Private GHCR runtime images

The current protected application artifacts are stored in private GHCR packages and pinned by immutable digest.

- API: `ghcr.io/christyepez/agendamiento-mkt-api@sha256:fa46b1423cea1b87b397bd4cd4077ce9910227e0092964875201c3f0e72a9a77`
- Web: `ghcr.io/christyepez/agendamiento-mkt-web@sha256:8c913e9a2e5d46f2ed28227a227cc1804f58036526aca8fc3704f67fc2e65f89`

The root `docker-compose.yml` currently only contains the SQL initialization helper and reuses the existing Requirements SQL Server. These application images are documented here rather than adding inactive services to that compose file.
