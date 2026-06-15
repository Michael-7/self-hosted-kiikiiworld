# Building the kiikiiworld backend in ASP.NET — a learn-by-doing guide

This is a self-paced guide to rebuild the serverless backend as **one small ASP.NET
Core service** you run in Docker at home. You'll type everything yourself; each phase
explains *what* you're doing and *why*, then gives a way to verify it works before
moving on.

**Decisions this guide assumes** (from earlier planning):

- **Minimal API** (not MVC controllers) — least ceremony for a small service.
- **SQLite + EF Core** — the whole database is one file on disk.
- **No authentication** — the authoring API is local-only; writes never face the internet.
- **ImageSharp** for image resizing, **ffmpeg** for video transcoding.
- **Hybrid hosting** — this backend is your *authoring + media* tool; the public blog
  is the static Next.js export.

Work top to bottom. Don't skip the "Verify" boxes — they're how you learn what each
piece actually does.

---

## Phase 0 — Setup and mental model

### Install the tools

1. **.NET SDK** — install the latest LTS (.NET 10 as of 2026; any recent version is
   fine, the commands are identical).
   Verify:
   ```bash
   dotnet --version
   ```
2. **Docker Desktop** (you'll only need it from Phase 8 onward).
3. An editor: **Rider** (you already use JetBrains) or **VS Code + C# Dev Kit**.

### The mental model (read once)

- **ASP.NET Core** is the web framework. **Kestrel** is the built-in web server that
  listens for HTTP — there's no separate nginx/IIS needed during development.
- A **Minimal API** app is basically two objects:
  - `builder` — where you *register services* (database, CORS, etc.) before startup.
  - `app` — where you *map routes* (`GET /posts`, `POST /media/image`, …) and run.
- **Dependency Injection (DI)**: you register a thing once (e.g. the database context),
  and ASP.NET hands it to any endpoint that asks for it by putting it in the parameter
  list. You'll see this in Phase 3 — it's the one concept that makes everything click.

How this maps to what you're replacing:

| Old (AWS) | New (this service) |
|---|---|
| 5 Lambdas + API Gateway | one ASP.NET app, many routes |
| DynamoDB | SQLite file |
| S3 + presigned URLs | local folder + multipart upload |
| `sharp` Lambda | ImageSharp, inline on upload |
| auth + JWT + Secrets Manager | *(deleted)* |

---

## Phase 1 — Hello, API

**Goal:** create the project, run it, understand the `builder`/`app` shape.

```bash
# from wherever you keep code; this creates a new folder
dotnet new web -o KiikiiApi
cd KiikiiApi
dotnet run
```

`dotnet new web` is the Minimal API template. When it starts it prints a line like
`Now listening on: http://localhost:5xxx`. Open that URL — you'll see `Hello World!`.

Open `Program.cs`. It's tiny:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
```

Now add a couple of routes to learn the basics. Replace the `MapGet("/")` line with:

```csharp
app.MapGet("/", () => "kiikii api up");

// route parameter: /hello/mike  ->  "hi mike"
app.MapGet("/hello/{name}", (string name) => $"hi {name}");

// query parameter: /year?value=2024  ->  JSON
app.MapGet("/year", (int value) => Results.Ok(new { year = value }));
```

> **Verify:** stop the app (Ctrl-C), `dotnet run` again, and hit `/hello/yourname`
> and `/year?value=2024` in the browser. Notice ASP.NET parsed the route param and
> the query param into method arguments **for you**, and `Results.Ok(...)` returned
> JSON. That parameter-binding magic is the core of Minimal APIs.

**What you learned:** the builder/app lifecycle, routing, route vs query params,
returning text vs JSON (`Results.Ok`).

> Tip: `dotnet watch run` restarts automatically when you save — use it from now on.

---

## Phase 2 — The Post model, in memory first

**Goal:** model a blog post and build full CRUD against an in-memory list, so you learn
the HTTP verbs before adding a database.

We'll mirror the **original storage shape** because the frontend already understands it
and it keeps the flexible `body` (a string for quotes/stories/video, an array for image
posts). A post is split into:

- **query columns**: `Id`, `Type`, `Date` (so we can look posts up)
- **`Content`**: a JSON blob holding everything else (`title`, `body`, `meta`)

Make a file `Post.cs`:

```csharp
public class Post
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";   // quote | story | image | video
    public string Date { get; set; } = "";   // e.g. "2024-06-15T12:00"
    public string Content { get; set; } = ""; // JSON: { title, body, meta }
}
```

The frontend sends a flat JSON post like
`{ id, type, date, title, body, meta }`. So on write we *peel off* id/type/date and
stuff the rest into `Content`; on read we merge them back. Add helpers in `Program.cs`
(above `app.Run()`), using the built-in `System.Text.Json`:

```csharp
using System.Text.Json;

// in-memory store for now
var posts = new List<Post>();

// turn an incoming flat post (JSON) into our stored shape
Post ToStored(JsonElement body)
{
    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body)!;
    var id   = dict["id"].GetString()!;
    var type = dict["type"].GetString()!;
    var date = dict["date"].GetString()!;
    foreach (var k in new[] { "id", "type", "date" }) dict.Remove(k);
    return new Post { Id = id, Type = type, Date = date,
                      Content = JsonSerializer.Serialize(dict) };
}

// turn a stored post back into the flat shape the frontend expects
object ToFlat(Post p)
{
    var content = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(p.Content)!;
    var result = new Dictionary<string, object?> { ["id"] = p.Id, ["type"] = p.Type, ["date"] = p.Date };
    foreach (var kv in content) result[kv.Key] = kv.Value;
    return result;
}
```

Now the CRUD routes:

```csharp
// list by year (date starts with "2024") or by type
app.MapGet("/posts", (string? postYear, string? type) =>
{
    IEnumerable<Post> q = posts;
    if (postYear is not null) q = q.Where(p => p.Date.StartsWith(postYear));
    if (type is not null)     q = q.Where(p => p.Type == type);
    return Results.Ok(q.Select(ToFlat));
});

app.MapPost("/posts", (JsonElement body) =>
{
    posts.Add(ToStored(body));
    return Results.Ok();
});

app.MapPatch("/posts", (JsonElement body) =>
{
    var incoming = ToStored(body);
    var existing = posts.FirstOrDefault(p => p.Id == incoming.Id);
    if (existing is null) return Results.NotFound();
    existing.Content = incoming.Content;
    return Results.Ok();
});

app.MapDelete("/posts", (string id) =>
{
    posts.RemoveAll(p => p.Id == id);
    return Results.Ok();
});
```

> **Verify** with `curl` (or your IDE's HTTP client):
> ```bash
> curl -X POST localhost:5xxx/posts \
>   -H "Content-Type: application/json" \
>   -d '{"id":"1","type":"quote","date":"2024-06-15","title":"hi","body":"hello","meta":{"hide":false}}'
> curl "localhost:5xxx/posts?postYear=2024"
> ```
> You should get your post back in the flat shape. Try `?type=quote`, then `DELETE`.

**What you learned:** the four HTTP verbs as routes, reading a raw JSON body
(`JsonElement`), and the store/flat transform that keeps `body` flexible. The data
vanishes on restart — that's what Phase 3 fixes.

---

## Phase 3 — Real storage with SQLite + EF Core

**Goal:** swap the in-memory list for a SQLite file, and meet Dependency Injection.

EF Core is the ORM: you describe your data as C# classes and it generates the SQL.
Add the packages:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Create `AppDbContext.cs` — this is your database session/handle:

```csharp
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Post> Posts => Set<Post>(); // maps to a "Posts" table
}
```

Tell EF the `Id` is the key — add `[Key]` or rely on convention (a property named `Id`
is the primary key automatically, so you're already fine).

Register it with DI and create the database on startup. Near the top of `Program.cs`:

```csharp
var dataDir = builder.Configuration["DataDir"] ?? "data";
Directory.CreateDirectory(dataDir);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite($"Data Source={Path.Combine(dataDir, "blog.db")}"));

var app = builder.Build();

// create the file + table if missing (simple path; see note on migrations)
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
```

Now rewrite the routes to use the database. The key new idea: **add an `AppDbContext db`
parameter** and ASP.NET injects it. EF calls are `async`, so endpoints become `async`:

```csharp
app.MapGet("/posts", async (AppDbContext db, string? postYear, string? type) =>
{
    var q = db.Posts.AsQueryable();
    if (postYear is not null) q = q.Where(p => p.Date.StartsWith(postYear));
    if (type is not null)     q = q.Where(p => p.Type == type);
    var rows = await q.ToListAsync();
    return Results.Ok(rows.Select(ToFlat));
});

app.MapPost("/posts", async (AppDbContext db, JsonElement body) =>
{
    db.Posts.Add(ToStored(body));
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapPatch("/posts", async (AppDbContext db, JsonElement body) =>
{
    var incoming = ToStored(body);
    var existing = await db.Posts.FindAsync(incoming.Id);
    if (existing is null) return Results.NotFound();
    existing.Content = incoming.Content;
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapDelete("/posts", async (AppDbContext db, string id) =>
{
    var p = await db.Posts.FindAsync(id);
    if (p is not null) { db.Posts.Remove(p); await db.SaveChangesAsync(); }
    return Results.Ok();
});
```

Delete the old in-memory `var posts = new List<Post>();`.

> **Verify:** POST a couple of posts, GET them, then **stop and restart** the app and
> GET again — they're still there. Open `data/blog.db` with any SQLite viewer (or
> `sqlite3 data/blog.db "select * from Posts;"`) to see your rows.

**What you learned:** EF Core entities → tables, `DbContext` as the DB handle,
**DI injecting it into endpoints**, and async data access. Your data now persists —
this file is later what lives on the Docker volume.

> **Note on migrations:** `EnsureCreated()` is the easy path but won't update the schema
> if you later change `Post`. The "proper" way is migrations:
> `dotnet tool install --global dotnet-ef`, then
> `dotnet ef migrations add Init` and `dotnet ef database update`. Switch to that once
> your model stabilizes.

---

## Phase 4 — Let the frontend talk to it (CORS)

**Goal:** allow the Next.js dev site to call this API from the browser.

Browsers block cross-origin calls unless the server opts in. Register a CORS policy:

```csharp
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:3000")  // Next.js dev server
     .AllowAnyHeader()
     .AllowAnyMethod()));
```

And enable it (after `var app = builder.Build();`, before the routes):

```csharp
app.UseCors();
```

Frontend side (small changes — backend guide, so just the gist):

- `frontend/.env`: `APIGATEWAY=http://localhost:5xxx`
- `mapPosts()` in `types/post.ts`: your API already returns the **flat** shape, so this
  collapses to roughly `return data;` — no more `.DateId.S` / `Content.S` parsing.

> **Verify:** run the frontend (`npm run dev` in `frontend/`) and load the homepage —
> posts you created should render. If the browser console shows a CORS error, re-check
> the origin string and that `app.UseCors()` runs before your routes.

**What you learned:** why CORS exists and how to scope it to your own origin.

---

## Phase 5 — Image upload + ImageSharp

**Goal:** accept an image file, resize it to 640px webp (like the old `sharp` Lambda),
store it, and serve it back.

```bash
dotnet add package SixLabors.ImageSharp
```

Decide a layout under your data dir:

```
data/media/images/optimized/<id>.webp
```

The upload endpoint. Note two gotchas baked in:

- `.DisableAntiforgery()` — Minimal APIs require an antiforgery token for form uploads
  by default; for a local tool, switch it off or uploads 400.
- We process straight into webp and never keep the original (matches the old behavior).

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;

var imageDir = Path.Combine(dataDir, "media", "images", "optimized");
Directory.CreateDirectory(imageDir);

app.MapPost("/media/image", async (IFormFile file) =>
{
    var id = Guid.NewGuid().ToString("N");
    using var stream = file.OpenReadStream();
    using var image = await Image.LoadAsync(stream);

    image.Mutate(x => x.Resize(new ResizeOptions
    {
        Size = new Size(640, 0),     // width 640, height auto
        Mode = ResizeMode.Max        // never upscale
    }));

    var path = Path.Combine(imageDir, $"{id}.webp");
    await image.SaveAsWebpAsync(path, new WebpEncoder { Quality = 95 });

    // return the public URL the frontend should store in the post body
    return Results.Ok(new { url = $"/media/images/optimized/{id}.webp" });
}).DisableAntiforgery();
```

Now serve the saved files as static content. Add near the top after `var app = ...`:

```csharp
using Microsoft.Extensions.FileProviders;

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(dataDir, "media")),
    RequestPath = "/media"
});
```

> **Verify:**
> ```bash
> curl -F "file=@/path/to/photo.jpg" localhost:5xxx/media/image
> ```
> You get back `{ "url": "/media/images/optimized/xxxx.webp" }`. Open that URL in the
> browser — your resized webp loads. Check the file appeared under `data/media/images/optimized/`.

**Frontend side:** in `post.tsx`, the image flow changes from "ask for a presigned S3
URL, then PUT to S3" to a single `POST` of the file to `/media/image`; push the returned
`url` into the post's `body` array.

**What you learned:** multipart file upload (`IFormFile`), in-process image transform,
the antiforgery gotcha, and serving a folder as static files.

---

## Phase 6 — Video upload + ffmpeg transcode + range serving

**Goal:** the new feature. Accept a (large) video, transcode it to a web-friendly MP4,
grab a poster frame, and serve it with **seeking** support.

### 6a. Raise the upload limits

Videos are big; the default request cap will reject them. Configure Kestrel and form
limits near the top:

```csharp
using Microsoft.AspNetCore.Http.Features;

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 2_000_000_000); // ~2 GB
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 2_000_000_000);
```

### 6b. ffmpeg

ffmpeg is an external program you call as a process. Install it locally to develop
(`brew install ffmpeg` on macOS); in Phase 8 it goes into the Docker image.

A tiny helper to run it and wait:

```csharp
using System.Diagnostics;

async Task RunFfmpeg(string args)
{
    var p = Process.Start(new ProcessStartInfo("ffmpeg", args)
    {
        RedirectStandardError = true, // ffmpeg logs to stderr
        UseShellExecute = false
    })!;
    await p.WaitForExitAsync();
    if (p.ExitCode != 0) throw new Exception("ffmpeg failed: " + await p.StandardError.ReadToEndAsync());
}
```

### 6c. The upload endpoint

Save the upload, then produce `video.mp4` (H.264/AAC, `+faststart` so it plays before
fully downloading) and a `poster.webp`.

```csharp
var videoRoot = Path.Combine(dataDir, "media", "videos");
Directory.CreateDirectory(videoRoot);

app.MapPost("/media/video", async (IFormFile file) =>
{
    var id  = Guid.NewGuid().ToString("N");
    var dir = Path.Combine(videoRoot, id);
    Directory.CreateDirectory(dir);

    // stream the upload to disk (don't load it all into memory)
    var source = Path.Combine(dir, "source" + Path.GetExtension(file.FileName));
    await using (var fs = File.Create(source))
        await file.CopyToAsync(fs);

    var mp4    = Path.Combine(dir, "video.mp4");
    var poster = Path.Combine(dir, "poster.webp");

    await RunFfmpeg($"-i \"{source}\" -c:v libx264 -c:a aac -movflags +faststart \"{mp4}\"");
    await RunFfmpeg($"-i \"{source}\" -ss 00:00:01 -vframes 1 \"{poster}\"");

    return Results.Ok(new
    {
        video  = $"/media/video/{id}",        // played from this box
        poster = $"/media/videos/{id}/poster.webp" // published to the static site
    });
}).DisableAntiforgery();
```

> **Start synchronous** like this so you understand the flow. A big transcode will make
> the request hang for a while — that's fine for learning. Later, move the two
> `RunFfmpeg` calls into a background task (an `IHostedService` queue) and return
> immediately with a "processing" status.

### 6d. Serve the video with range support

Range support is what lets the browser scrub/seek. `Results.File(..., enableRangeProcessing: true)`
does the heavy lifting:

```csharp
app.MapGet("/media/video/{id}", (string id) =>
{
    var path = Path.Combine(videoRoot, id, "video.mp4");
    if (!File.Exists(path)) return Results.NotFound();
    return Results.File(File.OpenRead(path), "video/mp4", enableRangeProcessing: true);
});
```

(The poster is already served by the static-files middleware from Phase 5.)

> **Verify:**
> ```bash
> curl -F "file=@/path/to/clip.mov" localhost:5xxx/media/video
> ```
> You get `{ video, poster }`. Open the `video` URL in the browser — it should play
> **and let you drag the scrubber** (that's range working). Check `data/media/videos/<id>/`
> holds `video.mp4` + `poster.webp`.

**Frontend side:** add a video file input for the `video` post type, store `video` as
the post body and `poster` in `meta`; render `<video controls poster={...} src={...}>`.
Because the poster lives on the static site, the post still looks fine when this box is off.

**What you learned:** large streamed uploads + limits, shelling out to ffmpeg, and HTTP
range requests for seekable media — the core of self-hosted video.

---

## Phase 7 — Tidy configuration

**Goal:** make paths and origins config-driven instead of hardcoded, so dev and Docker
differ only by settings.

In `appsettings.json` add:

```json
{
  "DataDir": "data",
  "AllowedOrigin": "http://localhost:3000"
}
```

Read `AllowedOrigin` in the CORS setup (`builder.Configuration["AllowedOrigin"]`). In
Docker you'll override `DataDir` to `/data` via an environment variable — no code change.
Environment variables override `appsettings.json` automatically; that's the pattern that
replaces AWS Secrets Manager / per-env config.

> **Verify:** run with `DataDir=/tmp/kiikii dotnet run` and confirm the db + media land
> in `/tmp/kiikii`.

**What you learned:** the configuration system and env-var overrides.

---

## Phase 8 — Put it in a container

**Goal:** package the app + ffmpeg into an image, and run it with a bind-mount volume so
data survives `stop`/`rm`.

Create `Dockerfile` in the project folder:

```dockerfile
# build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV DataDir=/data
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "KiikiiApi.dll"]
```

Two things to notice: the **runtime image installs ffmpeg** (so your `Process.Start("ffmpeg")`
works inside the container), and `DataDir=/data` points at the volume.

Create `docker-compose.yml`:

```yaml
services:
  api:
    build: .
    ports:
      - "127.0.0.1:5000:8080"   # bound to localhost ONLY — not exposed to the network
    volumes:
      - ./data:/data            # db + media live on the host, survive the container
    environment:
      - AllowedOrigin=http://localhost:3000
```

The `127.0.0.1:` prefix is your security model now (instead of auth): the write API is
only reachable from your own machine.

Run it:

```bash
docker compose up -d --build   # start (build first time)
# ... author, upload ...
docker compose stop            # stop; data stays in ./data
docker compose up -d           # back up later, same data
```

> **Verify:** with the container up, repeat a POST and a GET against
> `localhost:5000`. Then `docker compose down` (removes the container) and
> `docker compose up -d` again — your posts and media are still there because they're in
> `./data`, not in the container. **That's the whole "spin up / stop" workflow working.**

**What you learned:** multi-stage builds, adding a system dependency (ffmpeg) to the
runtime image, host bind-mounts for persistence, and localhost-only port binding as your
security boundary. **Back up = copy the `data/` folder.**

---

## Phase 9 — Where to go next (pointers, not steps)

You now have the backend. The remaining pieces from the plan:

- **Publish pipeline:** a `POST /publish` endpoint (or a script) that exports posts to
  `posts.json`, copies optimized images + posters into the Next.js `out/`, runs the
  static build, and deploys it to your always-on host. This is what makes the public
  site work while this box is off.
- **Background transcode:** move the two `RunFfmpeg` calls off the request thread into an
  `IHostedService` queue so uploads return instantly and you can poll status.
- **Cloudflare Tunnel:** add a `cloudflared` service to compose so the public `<video>`
  `src` reaches this box — route **only** `/media/video/*`, never the write endpoints.
- **Migrations:** switch from `EnsureCreated()` to EF migrations once the schema settles.
- **Remote authoring (optional):** if you ever want to post from your phone, put
  Cloudflare Access in front of the write routes — much simpler than the old passkeys.

---

## Cheat sheet

```bash
dotnet new web -o KiikiiApi      # new project
dotnet watch run                 # run + auto-reload
dotnet add package <name>        # add a library
dotnet ef migrations add <name>  # (after switching to migrations)
docker compose up -d --build     # build + start container
docker compose stop              # stop, keep data
sqlite3 data/blog.db "select * from Posts;"   # peek at the db
```

Build order, one line each: **hello → in-memory CRUD → SQLite → CORS → images →
video → config → Docker.** Get each "Verify" green before the next phase.
