import AppError from '@shared/errors/AppError';
import { compare, hash } from 'bcryptjs';
import { getCustomRepository } from 'typeorm';
import User from '../typeorm/entities/User';
import UsersRepository from '../typeorm/repositories/UsersRepository';

interface IRequest {
  user_id: string;
}

export default class ShowProfileService {
  public async execute({ user_id }: IRequest): Promise<User> {
    //instancia o getCustomRepositury UserRepository
    const usersRepository = getCustomRepository(UsersRepository);

    //executa o método find da instância do usersRepository
    const user = await usersRepository.findById(user_id);

    if (!user) {
      throw new AppError('User no found');
    }

    //retorna um array de usuarios
    return user;
  }
}
