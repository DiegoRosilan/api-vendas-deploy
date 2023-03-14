import AppError from '@shared/errors/AppError';
import { hash } from 'bcryptjs';
import { getCustomRepository } from 'typeorm';
import User from '../typeorm/entities/User';
import UsersRepository from '../typeorm/repositories/UsersRepository';

//interface IRequest
interface IRequest {
  name: string;
  email: string;
  password: string;
}

export default class CreateUserService {
  public async execute({ name, email, password }: IRequest): Promise<User> {
    //instancia o getCustomRepository UserRepository
    const usersRepository = getCustomRepository(UsersRepository);

    //executa o método findByName da instância userRepository
    const emailExist = await usersRepository.findByEmail(email);

    //verifica se já existe um email igual cadastrado
    //se for true libera a excessão AppError
    if (emailExist) {
      throw new AppError('There is already User with this email');
    }

    const hashedPassword = await hash(password, 8);

    //executa o metodo create do respositorio
    const user = usersRepository.create({
      name,
      email,
      password: hashedPassword,
    });

    //salva o user no banco
    await usersRepository.save(user);

    //retorna um objeto User
    return user;
  }
}
