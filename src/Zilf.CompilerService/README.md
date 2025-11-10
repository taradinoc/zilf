# ZILF Compiler Service

An ASP.NET Core minimal API that compiles ZIL projects using the ZILF toolchain and returns the compiled storyfile and logs. It is designed to run in Docker, similar in concept to the Inform 7 service at https://github.com/borogove-if/i7-compiler-service.

## Endpoints

### Monitoring Endpoints

- **GET /monitoring/ping** — responds with `"pong"` for basic availability checks
- **GET /monitoring/version** — returns ZILF version, .NET framework, and OS info
  
  Response JSON:
  ```json
  {
    "version": "0.9.1",
    "framework": ".NET 9.0.0",
    "os": "Linux 6.5.0-1025-azure #26~22.04.1-Ubuntu SMP Thu Jul 11 22:33:04 UTC 2024"
  }
  ```

- **GET /monitoring/storage** — checks disk space usage
  - Returns HTTP 200 with status JSON if space usage is below threshold
  - Returns HTTP 500 if disk usage >= threshold (default 99%)
  - Set threshold via `DISKSPACE_ALERT_THRESHOLD` environment variable
  
  Response JSON (success):
  ```json
  {
    "status": "ok",
    "usedPercent": 45.67,
    "threshold": 99.0
  }
  ```

### Compilation API

- **POST /prepare** — create or overwrite a project by uploading source files
  
  Request JSON:
  ```json
  {
    "data": { 
      "sessionId": "my-game-123",
      "debug": false,
      "language": "zil",
      "uuid": "optional-uuid"
    },
    "included": [ 
      { 
        "type": "file", 
        "attributes": { 
          "name": "story.zil", 
          "directory": "",
          "contents": "<ROUTINE GO () <TELL \"Hello world\"> <CRLF>>"
        } 
      },
      { 
        "type": "file", 
        "attributes": { 
          "name": "parser.zil", 
          "directory": "lib",
          "contents": "..."
        } 
      }
    ]
  }
  ```
  
  Response JSON: 
  ```json
  { "jobId": "my-game-123" }
  ```

- **GET /compile/{jobId}** — compile a prepared project
  
  Query parameters:
  - `mainFile` (optional): relative path to the root ZIL source file within the project (e.g., `src/game/story.zil`, `story.zil`). Supports subdirectories. Must stay within the project (no `..` traversal, no absolute paths).
  - `debug` (optional): boolean (`true` or `false`) to request debug info in the compiled output.
  
  If `mainFile` is omitted, the service searches recursively for `story.zil`, otherwise uses the first `*.zil` file found.
  
  The resulting `.zap` file is written alongside the specified main file (preserving subdirectory structure), then assembled to a Z-machine story file (`.z3`, `.z5`, or `.z8` depending on the `VERSION` directive in the ZIL source).
  
  Response JSON:
  ```json
  {
    "success": true,
    "jobId": "my-game-123",
    "storyfileUrl": "/results/my-game-123/storyfile/story.z3",
    "log": "[warning ZIL0213] /work/Projects/asdfghjkl/hello/hello.zil:1: routine 'UNUSED' is defined but never used\n",
    "messages": [
      { "severity": "warning", "code": "ZIL0213", "text": "/work/Projects/asdfghjkl/hello/hello.zil:1: warning ZIL0213: routine 'UNUSED' is defined but never used" }
    ]
  }
  ```

- **GET /results/{jobId}/storyfile/{file}** — download the compiled Z-machine storyfile
  - Returns the binary story file with `Content-Type: application/octet-stream`

- **GET /results/{jobId}/log** — download the compilation log as plain text
  - Returns the log with `Content-Type: text/plain`

## Build & Run (Docker)

Build the image from the repository root:

```bash
# Build
docker build -f src/Zilf.CompilerService/Dockerfile -t zilf-compiler-service .

# Run (Linux/macOS)
docker run --rm -p 8080:8080 \
  -e ZILF_WORK_ROOT=/data \
  -e DISKSPACE_ALERT_THRESHOLD=90 \
  -v $(pwd)/zillib:/app/zillib \
  -v $(pwd)/.cache:/data \
  zilf-compiler-service

# Run (Windows PowerShell)
docker run --rm -p 8080:8080 `
  -e ZILF_WORK_ROOT=/data `
  -e DISKSPACE_ALERT_THRESHOLD=90 `
  -v ${PWD}/zillib:/app/zillib `
  -v ${PWD}/.cache:/data `
  zilf-compiler-service
```

Then test the API:

```bash
# Prepare a project
curl -X POST http://localhost:8080/prepare \
  -H "Content-Type: application/json" \
  -d '{
    "data": {"sessionId": "test-123"},
    "included": [{
      "type": "file",
      "attributes": {
        "name": "story.zil",
        "directory": "",
        "contents": "<VERSION ZIP><ROUTINE GO () <TELL \"Hello!\"> <CRLF>>"
      }
    }]
  }'

# Compile the project
curl http://localhost:8080/compile/test-123

# Compile with specific main file and debug info
curl "http://localhost:8080/compile/test-123?mainFile=src/game.zil&debug=true"

# Download the storyfile
curl -O http://localhost:8080/results/test-123/storyfile/story.z3

# View the log
curl http://localhost:8080/results/test-123/log
```

## Scheduled Cleanup

The Docker container includes a cron job that runs daily at midnight UTC to clean up old build files (older than 1 day) from the Projects and Stash directories. Logs are written to `/var/log/cron.log` inside the container.

## Notes

- The service copies the repository's `zillib/` into the image and adds it to include paths automatically.
- For multi-file projects, include all files in the `/prepare` request with appropriate `directory` attributes to preserve structure.
- You may specify the `mainFile` query parameter in the `/compile` request; if omitted, the service searches for `story.zil` recursively, then falls back to the first `*.zil` file found.
- Session IDs (jobId) must be alphanumeric with optional hyphens, max 100 characters.
- The `.zap` intermediate file is written alongside the main ZIL source file, preserving subdirectory structure.
- No Z-machine version needs to be specified—the assembler reads the `VERSION` directive from your ZIL source and produces the appropriate `.z3`, `.z5`, or `.z8` file automatically.
- Compiled storyfiles and logs are stored in `ZILF_WORK_ROOT/Stash/{jobId}/` and are subject to automatic cleanup (see below).

## Environment Variables

- `ZILF_WORK_ROOT` (default: `/work`) — base directory for project files and compilation outputs
- `DISKSPACE_ALERT_THRESHOLD` (default: `99.0`) — disk usage percentage threshold for `/monitoring/storage` to return HTTP 500
