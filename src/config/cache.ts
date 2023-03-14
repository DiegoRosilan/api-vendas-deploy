import { RedisOptions } from 'ioredis';

//interface para pegar o RedisOptions do pacote ioredis
interface ICacheConfig {
  config: {
    redis: RedisOptions;
  };

  driver: string;
}

export default {
  //configuração do redis
  config: {
    redis: {
      host: process.env.REDIS_HOST,
      port: process.env.REDIS_PORT,
      password: process.env.REDIS_PASS || undefined,
    },
  },
  driver: 'redis',
} as ICacheConfig;
