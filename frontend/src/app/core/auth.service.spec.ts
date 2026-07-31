import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should start with no authenticated user', () => {
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.isEditor()).toBeFalse();
    expect(service.isAdmin()).toBeFalse();
  });

  it('loadUser populates the current user', () => {
    service.loadUser();

    const req = httpMock.expectOne('/api/auth/me');
    req.flush({ id: '1', email: 'test@example.com', displayName: 'Test', roles: ['Editor'] });

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.isEditor()).toBeTrue();
    expect(service.user()?.email).toBe('test@example.com');
  });

  it('loadUser clears the user on error', () => {
    service.loadUser();

    const req = httpMock.expectOne('/api/auth/me');
    req.error(new ProgressEvent('error'), { status: 500 });

    expect(service.isAuthenticated()).toBeFalse();
  });
});
