const http = require('node:http')
const path = require('node:path')
const { spawn } = require('node:child_process')

const repoRoot = path.resolve(__dirname, '..', '..', '..')
const e2eRoot = path.resolve(__dirname, '..')
const baseUrl = process.env.CYPRESS_BASE_URL || 'http://localhost:5177'
const connectionString =
  process.env.ConnectionStrings__CraftsmanDb ||
  'Host=localhost;Port=5433;Database=craftsman_e2e;Username=craftsman;Password=craftsman_dev_password'

let appProcess

main().catch(async error => {
  console.error(error)
  process.exit(1)
})

async function main() {
  try {
    await run('docker', ['compose', '-f', 'infra/docker-compose.e2e.yml', 'up', '-d', 'postgres_e2e'], repoRoot)

    appProcess = spawn(
      'dotnet',
      [
        'run',
        '--project',
        'src/App/Craftsman.csproj',
        '--no-launch-profile',
        '--urls',
        baseUrl
      ],
      {
        cwd: repoRoot,
        stdio: 'inherit',
        env: {
          ...process.env,
          ASPNETCORE_ENVIRONMENT: 'E2E',
          ASPNETCORE_URLS: baseUrl,
          ConnectionStrings__CraftsmanDb: connectionString
        }
      }
    )

    await waitForHealth(`${baseUrl}/health`, 120000)
    await run('node', ['node_modules/cypress/bin/cypress', 'run'], e2eRoot, {
      ...process.env,
      CYPRESS_BASE_URL: baseUrl
    })
  } finally {
    await cleanup()
  }
}

function run(command, args, cwd, env = process.env) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { cwd, env, stdio: 'inherit' })
    child.on('exit', code => {
      if (code === 0) {
        resolve()
        return
      }

      reject(new Error(`${command} ${args.join(' ')} failed with exit code ${code}`))
    })
    child.on('error', reject)
  })
}

function waitForHealth(url, timeoutMs) {
  const startedAt = Date.now()

  return new Promise((resolve, reject) => {
    const check = () => {
      const request = http.get(url, response => {
        response.resume()
        if (response.statusCode === 200) {
          resolve()
          return
        }

        retry()
      })

      request.on('error', retry)
      request.setTimeout(2000, () => {
        request.destroy()
        retry()
      })
    }

    const retry = () => {
      if (Date.now() - startedAt > timeoutMs) {
        reject(new Error(`Timed out waiting for ${url}`))
        return
      }

      setTimeout(check, 1000)
    }

    check()
  })
}

async function stopApp() {
  if (!appProcess || appProcess.killed) {
    return
  }

  appProcess.kill()
  await new Promise(resolve => appProcess.once('exit', resolve))
}

async function cleanup() {
  await stopApp()

  try {
    await run('docker', ['compose', '-f', 'infra/docker-compose.e2e.yml', 'down'], repoRoot)
  } catch (error) {
    console.warn(`Failed to stop E2E Docker compose: ${error.message}`)
  }
}
