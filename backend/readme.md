# Backend KiiKiiWorld

run `dotnet watch run` inside Kiikiiworld.Api

TODO:

- ~~login for admin panel~~
- upload media to my server
    - audio
    - video
    - images
- create posts with the media

## Auth setup

`POST /posts` and `DELETE /posts/{id}` require an admin login (`POST /auth/login`). There's a single
admin account, configured via `Auth:AdminUsername` / `Auth:AdminPasswordHash` — not stored in the
database, and never committed to `appsettings*.json`.

Generate a bcrypt hash for your password with any bcrypt CLI/REPL, e.g.:

```bash
python3 -c "import bcrypt; print(bcrypt.hashpw(b'your-password', bcrypt.gensalt()).decode())"
```

**Local dev** — store it in User Secrets (already initialized for this project):

```bash
dotnet user-secrets set "Auth:AdminUsername" "your-username"
dotnet user-secrets set "Auth:AdminPasswordHash" "<bcrypt hash>"
```

**Hosting** — set environment variables instead:

```bash
Auth__AdminUsername=your-username
Auth__AdminPasswordHash=<bcrypt hash>
```

Login sets an HttpOnly cookie (`kiikiiworld_auth`, 7-day sliding expiration). `GET /posts` and
`GET /posts/{id}` stay public; everything else under `/posts` requires the cookie. `POST /auth/login`
is rate-limited (5 attempts/minute per IP) to slow brute forcing once this is internet-facing.

