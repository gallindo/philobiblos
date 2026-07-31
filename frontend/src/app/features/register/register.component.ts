import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { ErrorService } from '../../core/error.service';
import { ApiError } from '../../core/models';

type RegisterForm = FormGroup<{
  email: FormControl<string>;
  password: FormControl<string>;
  confirmPassword: FormControl<string>;
}>;

@Component({
  selector: 'app-register',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private readonly authService = inject(AuthService);
  private readonly errorService = inject(ErrorService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly form: RegisterForm = this.fb.group({
    email: this.fb.control<string>('', { validators: [Validators.required, Validators.email], nonNullable: true }),
    password: this.fb.control<string>('', {
      validators: [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).+$/)],
      nonNullable: true,
    }),
    confirmPassword: this.fb.control<string>('', { validators: [Validators.required], nonNullable: true }),
  }, { validators: [this.passwordsMatch] });

  readonly submitting = signal(false);
  readonly fieldErrors = signal<Record<string, string>>({});

  register(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorService.clear();
    this.fieldErrors.set({});

    const { email, password } = this.form.getRawValue();
    this.authService.register({ email, password }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigate(['/']);
      },
      error: (error: ApiError) => {
        this.submitting.set(false);
        this.fieldErrors.set(error.fieldErrors);
        if (error.nonFieldError) {
          this.errorService.show(error.nonFieldError);
        }
      },
    });
  }

  private passwordsMatch(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    return password && confirmPassword && password !== confirmPassword
      ? { passwordsMismatch: true }
      : null;
  }
}
