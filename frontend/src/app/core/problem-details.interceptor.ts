import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ErrorService } from './error.service';
import { ApiError } from './models';

function extractFieldErrors(errors: unknown): Record<string, string> {
  const fieldErrors: Record<string, string> = {};
  if (typeof errors !== 'object' || errors === null) {
    return fieldErrors;
  }

  for (const [key, messages] of Object.entries(errors)) {
    if (Array.isArray(messages) && messages.length > 0 && typeof messages[0] === 'string') {
      fieldErrors[key] = messages[0];
    }
  }
  return fieldErrors;
}

export const problemDetailsInterceptor: HttpInterceptorFn = (req, next) => {
  const errorService = inject(ErrorService);

  return next(req).pipe(
    catchError((response: HttpErrorResponse) => {
      const problem = response.error;

      if (
        problem !== null &&
        typeof problem === 'object' &&
        typeof problem.title === 'string' &&
        typeof problem.status === 'number'
      ) {
        if (problem.status === 400 && problem.errors) {
          const fieldErrors = extractFieldErrors(problem.errors);
          return throwError(() => ({ fieldErrors, nonFieldError: null } satisfies ApiError));
        }

        const message = problem.detail ?? problem.title ?? `Error ${problem.status}`;
        return throwError(() => ({ fieldErrors: {}, nonFieldError: message } satisfies ApiError));
      }

      const fallback = response.message || `An unexpected error occurred (${response.status})`;
      return throwError(() => ({ fieldErrors: {}, nonFieldError: fallback } satisfies ApiError));
    })
  );
};
