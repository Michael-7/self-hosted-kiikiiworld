# Backend KiiKiiWorld

run `dotnet watch run --project Kiikiiworld.Api` inside Kiikiiworld.Api

TODO:

- ~~login for admin panel~~
- upload media to my server
    - audio
    - video
    - images
- create posts with the media

## Auth setup

`POST /posts` and `DELETE /posts/{id}` require an admin login (`POST /auth/login`). There's a single
admin account, configured via `Auth:AdminUsername` / `Auth:AdminPassword` (plain text) — not stored
in the database.

**Local dev** — `appsettings.Development.json` already ships with `admin` / `admin`, so
`dotnet watch run` works with no setup. Change it there if you want something else locally; it's a
throwaway dev default, fine to keep committed.

**Hosting** — don't put real credentials in `appsettings.json`. Use User Secrets or environment
variables instead:

```bash
dotnet user-secrets set "Auth:AdminUsername" "your-username"
dotnet user-secrets set "Auth:AdminPassword" "your-password"
```

```bash
Auth__AdminUsername=your-username
Auth__AdminPassword=your-password
```

Login sets an HttpOnly cookie (`kiikiiworld_auth`, 7-day sliding expiration). `GET /posts` and
`GET /posts/{id}` stay public; everything else under `/posts` requires the cookie. `POST /auth/login`
is rate-limited (5 attempts/minute per IP) to slow brute forcing once this is internet-facing.

