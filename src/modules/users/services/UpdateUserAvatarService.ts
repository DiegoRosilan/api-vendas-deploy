import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import User from '../typeorm/entities/User';
import UsersRepository from '../typeorm/repositories/UsersRepository';
import uploadConfig from '@config/upload';
import DiskStorageProvider from '@shared/providers/StorageProviders/DiskStorageProvider';
import S3StorageProvider from '@shared/providers/StorageProviders/S3StorageProvider';

//interface IRequest
interface IRequest {
  user_id: string;
  avatarFilename: string;
}

export default class UpdateUserAvatarService {
  public async execute({ user_id, avatarFilename }: IRequest): Promise<User> {
    //instancia o getCustomRepository UserRepository
    const usersRepository = getCustomRepository(UsersRepository);

    //executa o metodo findById do repositório UserRepository
    const user = await usersRepository.findById(user_id);

    //verifica se o usuário existe
    if (!user) {
      throw new AppError('User not found');
    }

    if (uploadConfig.driver === 's3') {
      const storageProvider = new S3StorageProvider();
      //verifica se o usuário possui um avatar
      if (user.avatar) {
        await storageProvider.delete(user.avatar);
      }

      const fileName = await storageProvider.saveFile(avatarFilename);

      //seta o avatar para a coluna do banco
      user.avatar = fileName;
    } else {
      const diskProvider = new DiskStorageProvider();
      //verifica se o usuário possui um avatar
      if (user.avatar) {
        await diskProvider.delete(user.avatar);
      }

      const fileName = await diskProvider.saveFile(avatarFilename);

      //seta o avatar para a coluna do banco
      user.avatar = fileName;
    }

    //executa o metoto save do usersRepository
    await usersRepository.save(user);

    //retorna um user
    return user;
  }
}
