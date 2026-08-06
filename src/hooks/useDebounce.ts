import { useEffect, useState } from 'react';

/**
 * Hook to debounce a value with a specified delay.
 * Useful for reducing API calls when user is typing or making rapid changes.
 * 
 * @param value - The value to debounce
 * @param delay - The debounce delay in milliseconds (default: 500ms)
 * @returns The debounced value
 * 
 * @example
 * const debouncedAmount = useDebounce(amount, 500);
 * // Now use debouncedAmount in API queries
 */
export function useDebounce<T>(value: T, delay: number = 500): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    // Create a timer
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    // Clean up the timer if value changes or component unmounts
    return () => clearTimeout(handler);
  }, [value, delay]);

  return debouncedValue;
}
