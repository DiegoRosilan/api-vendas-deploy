import AppError from '@shared/errors/AppError';
import { compare } from 'bcryptjs';
import { sign } from 'jsonwebtoken';
import authConfig from '@config/auth';
import { getCustomRepository } from 'typeorm';
import User from '../typeorm/entities/User';
import UsersRepository from '../typeorm/repositories/UsersRepository';

//interface IRequest
interface IRequest {
  email: string;
  password: string;
}

interface IResponse {
  user: User;
  token: string;
}

export default class CreateSessionsService {
  public async execute({ email, password }: IRequest): Promise<IResponse> {
    //instancia o getCustomRepository UserRepository
    const usersRepository = getCustomRepository(UsersRepository);

    //executa o método findByName da instância userRepository
    const user = await usersRepository.findByEmail(email);

    //verifica se já existe um email igual cadastrado
    //se for true libera a excessão AppError
    if (!user) {
      throw new AppError('Incorrect email/password combination.', 401);
    }
    //compara a senha informada com a senha armazenada
    const passwordConfimed = await compare(password, user.password);

    if (!passwordConfimed) {
      throw new AppError('Incorrect email/password combination.', 401);
    }

    const token = sign({}, authConfig.jwt.secret, {
      subject: user.id,
      expiresIn: authConfig.jwt.expiresIn,
    });
    //retorna um objeto User
    return {
      user,
      token,
    };
  }
}
