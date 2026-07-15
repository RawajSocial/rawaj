// Mirrors Rawaj's JSend-style ApiResponse<T> envelope returned by every backend endpoint.
export interface ApiResponse<T> {
  status: 'success' | 'fail' | 'error';
  data: T | null;
  message: string | null;
  errors: Record<string, string[]> | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
