export class ApiError extends Error {
  constructor(
    message: string,
    readonly fieldErrors: Record<string, string[]> | null = null,
    readonly httpStatus: number | null = null,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}
