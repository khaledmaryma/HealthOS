import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthorizationService } from '../auth/authorization.service';
import { LoginResponse } from '../models/user-management';
import { UserManagementService } from '../services/user-management.service';

/**
 * Hero images are served from `public/images/` (URL `/images/...`).
 * Source artwork can live in `LIS.Web/Images`; run `npm run sync-login-images` to copy into `public/images`.
 */
const LOGIN_HERO_IMAGES: readonly { file: string; alt: string }[] = [
  { file: 'images.png', alt: 'Client branding' },
  { file: 'doctor-using-tablet-ai-healthcare-260nw-2669576405.webp', alt: 'Doctor using tablet for healthcare' },
  {
    file: 'doctor-busy-with-tablet-filling-out-checklist-in-hospital-setting-free-photo.jpeg',
    alt: 'Clinical documentation in hospital',
  },
  {
    file: 'health-care-patient-service-health-professional-physician-telephone-cartoon-thumbnail.jpg',
    alt: 'Patient service and care coordination',
  },
  {
    file: 'modern-healthcare-concept-doctor-controlling-260nw-2642241679.webp',
    alt: 'Modern healthcare and clinical oversight',
  },
  { file: 'modern-medical-professional-stockcake.webp', alt: 'Medical professional' },
  { file: 'istockphoto-953449116-612x612.jpg', alt: 'Healthcare setting' },
  {
    file: 'healthcare-manager-budgeting-improvement-medical-health-service-planning-improvements-to-better-patient-care-facilities-144615159.webp',
    alt: 'Health service planning and patient care facilities',
  },
  { file: 'istockphoto-979040452-612x612.jpg', alt: 'Medical care' },
  {
    file: 'pngtree-medical-practitioner-using-advanced-computer-interface-to-manage-patient-care-photo-image_32298558.jpg',
    alt: 'Digital patient care management',
  },
];

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  readonly username = signal('');
  readonly password = signal('');
  readonly isSubmitting = signal(false);
  readonly heroImageSrc = signal('');
  readonly heroImageAlt = signal('');

  constructor(
    private readonly svc: UserManagementService,
    private readonly auth: AuthorizationService,
    private readonly router: Router,
    private readonly location: Location
  ) {}

  ngOnInit(): void {
    const i = Math.floor(Math.random() * LOGIN_HERO_IMAGES.length);
    const pick = LOGIN_HERO_IMAGES[i];
    const path = this.location.prepareExternalUrl(`/images/${pick.file}`);
    this.heroImageSrc.set(path);
    this.heroImageAlt.set(pick.alt);
  }

  submit(event: Event): void {
    event.preventDefault();
    if (!this.username().trim() || !this.password().trim()) {
      alert('Username and password are required.');
      return;
    }

    this.isSubmitting.set(true);
    this.svc.login({ username: this.username(), password: this.password() }).subscribe({
      next: (user: LoginResponse) => {
        localStorage.setItem('loggedInUser', user.fullName || user.username);
        localStorage.setItem('loggedInUserId', user.id.toString());
        localStorage.setItem('userAccess', JSON.stringify(user.access));
        this.auth.setAdmin(!!user.isAdmin);
        if (user.departmentId != null) {
          localStorage.setItem('loggedInUserDepartmentId', String(user.departmentId));
        } else {
          localStorage.removeItem('loggedInUserDepartmentId');
        }
        if (user.departmentName) {
          localStorage.setItem('loggedInUserDepartmentName', user.departmentName);
        } else {
          localStorage.removeItem('loggedInUserDepartmentName');
        }
        console.log("🚀 ~ LoginComponent ~ submit ~ localStorage:", localStorage)

        console.log("🚀 ~ LoginComponent ~ submit ~ localStorage:" + JSON.stringify(localStorage));

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
