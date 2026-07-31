import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ErrorService } from './error.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const errorService = inject(ErrorService);

  return next(req).pipe(
    catchError((response: HttpErrorResponse) => {
      if (response.status === 401) {
        window.location.href = '/login';
      }

      if (response.status === 403) {
        const problem = response.error;
        const message =
          typeof problem === 'object' && problem?.detail
            ? problem.detail
            : 'You do not have permission to perform this action.';
        errorService.show(message);
      }

      return throwError(() => response);
    })
  );
};
