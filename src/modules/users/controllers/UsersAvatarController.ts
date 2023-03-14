import { Request, Response } from 'express';
import UpdateUserAvatarService from '../services/UpdateUserAvatarService';
import { instanceToInstance } from 'class-transformer';
export default class UsersAvatarController {
  //método para listagem
  public async update(request: Request, response: Response): Promise<Response> {
    //instancia do servico ListUsersService
    const updateAvatar = new UpdateUserAvatarService();

    const user = await updateAvatar.execute({
      user_id: request.user.id,
      avatarFilename: request.file?.filename as string,
    });
    return response.json(instanceToInstance(user));
  }
}
