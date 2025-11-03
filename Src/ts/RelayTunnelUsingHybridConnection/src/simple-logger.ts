export class SimpleLogger {
  constructor(private level: 'debug' | 'info' = 'info') {}

  debug(msg: string): void {
    if (this.level === 'debug') {
      console.log(msg)
    }
  }

  info(msg: string): void {
    console.log(msg)
  }

  warn(msg: string): void {
    console.log(`⚠ ${msg}`)
  }

  error(msg: string): void {
    console.error(`❌ ${msg}`)
  }

  child(_context: any): SimpleLogger {
    return this
  }
}
