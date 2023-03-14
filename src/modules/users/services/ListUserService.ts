import { getCustomRepository } from 'typeorm';
import User from '../typeorm/entities/User';
import UserRepository from '../typeorm/repositories/UsersRepository';

export default class ListUserService {
  public async execute(): Promise<User[]> {
    //instancia o getCustomRepositury UserRepository
    const usersRepository = getCustomRepository(UserRepository);

    //executa o método find da instância do usersRepository
    const users = await usersRepository.find();

    //retorna um array de usuarios
    return users;
  }
}
