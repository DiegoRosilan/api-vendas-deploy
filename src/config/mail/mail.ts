interface IMailConfig {
  driver: 'ethereal' | 'ses';

  defaults: {
    from: {
      email: string;
      name: string;
    };
  };
}

export default {
  driver: process.env.MAIL_DRIVER || 'ethereal',

  defaults: {
    from: {
      email: 'diegorosilan@adsoftwares.com.br',
      name: 'Diego Rosilan Santana Pinheiro',
    },
  },
} as IMailConfig;
