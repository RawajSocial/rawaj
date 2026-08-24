/** Mirrors the backend's generic `Rawaj.Common.PagedResult<T>` envelope. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
