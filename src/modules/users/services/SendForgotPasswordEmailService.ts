import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import UsersRepository from '../typeorm/repositories/UsersRepository';
import UserTokenRepository from '../typeorm/repositories/UserTokenRepository';
import EtherealMail from '@config/mail/EtherealMail';
import path from 'path';
import SESmail from '@config/mail/SESmail';
import mailConfig from '@config/mail/mail';

//interface IRequest
interface IRequest {
  email: string;
}

export default class SendForgotPasswordEmailService {
  public async execute({ email }: IRequest): Promise<void> {
    //instancia o getCustomRepository UserRepository
    const usersRepository = getCustomRepository(UsersRepository);
    //instancia o getCustomRepository UserTokenRepository
    const userTokenRepository = getCustomRepository(UserTokenRepository);

    const user = await usersRepository.findByEmail(email);
    //verifica se o email existe
    if (!user) {
      throw new AppError('User does not exists.');
    }
    //gera um novo token
    const token = await userTokenRepository.generate(user.id);

    const forgotPasswordTemplate = path.resolve(
      __dirname,
      '..',
      'views',
      'forgot_password.hbs',
    );

    if (mailConfig.driver === 'ses') {
      await SESmail.sendMail({
        to: {
          name: user.name,
          email: user.email,
        },
        subject: '[API Vendas] Recuperação de senha',
        templateData: {
          file: forgotPasswordTemplate,
          variables: {
            name: user.name,
            link: `${process.env.APP_WEB_URL}/reset_password?token=${token?.token}`,
          },
        },
      });
      return;
    }
    await EtherealMail.sendMail({
      to: {
        name: user.name,
        email: user.email,
      },
      subject: '[API Vendas] Recuperação de senha',
      templateData: {
        file: forgotPasswordTemplate,
        variables: {
          name: user.name,
          link: `${process.env.APP_WEB_URL}/reset_password?token=${token?.token}`,
        },
      },
    });
  }
}
