import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import UsersRepository from '../typeorm/repositories/UsersRepository';

//interface IRequest
interface IRequest {
  email: string;
}

export default class DeleteUsersService {
  public async execute({ email }: IRequest): Promise<void> {
    //instancia o getCustomRepositury userRepository
    const userRepository = getCustomRepository(UsersRepository);

    //executa o método findOne da instância do userRepository
    const user = await userRepository.findByEmail(email);

    //verifica se não existe um user
    //se for true libera a excessão AppError
    if (!user) {
      throw new AppError('user no found');
    }

    //executa o método remove da instância do userRepository
    await userRepository.remove(user);
  }
}
