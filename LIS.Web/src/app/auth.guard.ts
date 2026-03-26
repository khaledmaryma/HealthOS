import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthorizationService } from './auth/authorization.service';

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  const auth = inject(AuthorizationService);

  const userId = localStorage.getItem('loggedInUserId');
  if (!userId) {
    router.navigate(['/login']);
    return false;
  }

  // Admin: full access to all authenticated routes without further permission checks
  if (auth.isAdmin()) {
    return true;
  }

  return true;
};
