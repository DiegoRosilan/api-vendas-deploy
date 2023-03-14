import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import UsersRepository from '../typeorm/repositories/UsersRepository';
import UserTokenRepository from '../typeorm/repositories/UserTokenRepository';
import { isAfter, addHours } from 'date-fns';
import { hash } from 'bcryptjs';

//interface IRequest
interface IRequest {
  token: string;
  password: string;
}

export default class ResetPasswordService {
  public async execute({ token, password }: IRequest): Promise<void> {
    //instancia o getCustomRepository UserRepository
    const usersRepository = getCustomRepository(UsersRepository);
    //instancia o getCustomRepository UserTokenRepository
    const userTokenRepository = getCustomRepository(UserTokenRepository);

    //const user = await usersRepository.findByEmail(email);
    const userToken = await userTokenRepository.findByToken(token);
    //verifica se o Token é valido
    if (!userToken) {
      throw new AppError('User Token does not exists.');
    }
    //gera um novo token
    const user = await usersRepository.findById(userToken.user_id);

    //verifica se o usuário existe
    if (!user) {
      throw new AppError('User does not exists.');
    }

    const tokenCreatedAt = userToken.created_at;
    const compareDate = addHours(tokenCreatedAt, 2);

    if (isAfter(Date.now(), compareDate)) {
      throw new AppError('Token Expired');
    }
    //atuliaza a senha do usuário
    user.password = await hash(password, 8);
    //salva no banco de dados
    await usersRepository.save(user);
  }
}
