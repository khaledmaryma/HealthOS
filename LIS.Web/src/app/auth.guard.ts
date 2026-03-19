import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  const userId = localStorage.getItem('loggedInUserId');
  if (userId) {
    return true;
  }
  router.navigate(['/login']);
  return false;
};
