import { useState, useCallback } from 'react';
import type { ApiResponse } from '../types';

interface UseApiState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

export function useApi<T>() {
  const [state, setState] = useState<UseApiState<T>>({
    data: null,
    loading: false,
    error: null,
  });

  const execute = useCallback(
    async (apiCall: Promise<ApiResponse<T>>) => {
      setState({ data: null, loading: true, error: null });

      try {
        const response = await apiCall;

        if (response.success) {
          setState({ data: response.data, loading: false, error: null });
          return response.data;
        } else {
          setState({
            data: null,
            loading: false,
            error: response.error || 'An error occurred',
          });
          return null;
        }
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'An unexpected error occurred';
        setState({ data: null, loading: false, error: errorMessage });
        return null;
      }
    },
    []
  );

  const reset = useCallback(() => {
    setState({ data: null, loading: false, error: null });
  }, []);

  return {
    ...state,
    execute,
    reset,
  };
}

// Example usage:
// const { data, loading, error, execute } = useApi<CompanyData>();
// 
// useEffect(() => {
//   execute(datasetService.getCompany('NVDA'));
// }, []);
