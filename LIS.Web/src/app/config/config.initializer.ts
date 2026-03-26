import { APP_INITIALIZER, Provider } from '@angular/core';
import { ConfigService } from './config.service';

export function initConfigFactory(config: ConfigService): () => Promise<void> {
  return () => config.load();
}

export const CONFIG_INITIALIZER_PROVIDER: Provider = {
  provide: APP_INITIALIZER,
  multi: true,
  deps: [ConfigService],
  useFactory: initConfigFactory,
};

