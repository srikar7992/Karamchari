/**
 * Represents a company holiday.
 */
export interface Holiday {
  /** Unique identifier for the holiday. */
  id: string;
  /** Name of the holiday (e.g., "Christmas"). */
  name: string;
  /** Date of the holiday in ISO format (YYYY-MM-DD). */
  date: string;
}
