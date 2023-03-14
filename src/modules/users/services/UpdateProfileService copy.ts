import AppError from '@shared/errors/AppError';
import { compare, hash } from 'bcryptjs';
import { getCustomRepository } from 'typeorm';
import User from '../typeorm/entities/User';
import UsersRepository from '../typeorm/repositories/UsersRepository';

interface IRequest {
  user_id: string;
  name: string;
  email: string;
  password?: string;
  old_password?: string;
}

export default class UpdateProfileService {
  public async execute({
    user_id,
    name,
    email,
    password,
    old_password,
  }: IRequest): Promise<User> {
    //instancia o getCustomRepositury UserRepository
    const usersRepository = getCustomRepository(UsersRepository);

    //executa o método find da instância do usersRepository
    const user = await usersRepository.findById(user_id);

    if (!user) {
      throw new AppError('User no found');
    }

    const userUpdateEmail = await usersRepository.findByEmail(email);

    if (userUpdateEmail && userUpdateEmail.id !== user_id) {
      throw new AppError('There is already one user with this email.');
    }

    //verifica se foi informado a nova senha e a antiga
    if (password && !old_password) {
      throw new AppError('Old Pessword is required.');
    }

    //verifica se se as senhas foram informadas
    if (password && old_password) {
      //compara as senha para vê se não são iguais
      const checkOldPassword = await compare(old_password, user.password);

      //verifica se as senhas são iguas
      if (!checkOldPassword) {
        throw new AppError('Old Pessword no match,.');
      }
      //se as senha não forem iguas, atualiza a senha
      user.password = await hash(password, 8);
    }

    user.name = name;
    user.email = email;

    //salva
    await usersRepository.save(user);

    //retorna um array de usuarios
    return user;
  }
}
