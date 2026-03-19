import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UserManagementService } from '../services/user-management.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  readonly username = signal('');
  readonly password = signal('');
  readonly isSubmitting = signal(false);

  constructor(
    private readonly svc: UserManagementService,
    private readonly router: Router
  ) {}

  submit(event: Event): void {
    event.preventDefault();
    if (!this.username().trim() || !this.password().trim()) {
      alert('Username and password are required.');
      return;
    }

    this.isSubmitting.set(true);
    this.svc.login({ username: this.username(), password: this.password() }).subscribe({
      next: user => {
        localStorage.setItem('loggedInUser', user.fullName || user.username);
        localStorage.setItem('loggedInUserId', user.id.toString());
        localStorage.setItem('userAccess', JSON.stringify(user.access));
        this.isSubmitting.set(false);
        this.router.navigate(['/labtests']);
      },
      error: err => {
        console.error('Login failed', err);
        this.isSubmitting.set(false);
        alert('Invalid username or password.');
      }
    });
  }
}
