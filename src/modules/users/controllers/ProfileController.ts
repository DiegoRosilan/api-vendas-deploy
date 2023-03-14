import { Request, Response } from 'express';
import ShowProfileService from '../services/ShowProfileService';
import UpdateProfileService from '../services/UpdateProfileService copy';
import { instanceToInstance } from 'class-transformer';

export default class ProfileController {
  //método para listagem
  public async show(request: Request, response: Response) {
    //instancia do servico ShowProfileService
    const showProfile = new ShowProfileService();

    const user_id = request.user.id;
    //executa o método execute do service ShowProfile
    const User = await showProfile.execute({ user_id });

    //retorna um json
    return response.json(instanceToInstance(User));
  }

  public async update(request: Request, response: Response) {
    //variáveis pra receber os parametros do body
    const user_id = request.user.id;
    const { name, email, password, old_password } = request.body;

    //instancia do servico UpdateProfileService
    const updateProfile = new UpdateProfileService();

    //executa o método execute do service UpdateProfileService
    const User = await updateProfile.execute({
      user_id,
      name,
      email,
      password,
      old_password,
    });

    //retorna um json
    return response.json(instanceToInstance(User));
  }
}
