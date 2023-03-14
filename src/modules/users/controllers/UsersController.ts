import { Request, Response } from 'express';
import CreateUserService from '../services/CreateUserService';
import DeleteUsersService from '../services/DeleteUserService';
import ListUserService from '../services/ListUserService';
import { instanceToInstance } from 'class-transformer';

export default class UsersController {
  //método para listagem
  public async index(request: Request, response: Response) {
    //instancia do servico ListUsersService
    const listUsers = new ListUserService();

    //executa o método execute do service ListUsers
    const users = await listUsers.execute();

    //retorna um json
    return response.json(instanceToInstance(users));
  }

  public async create(request: Request, response: Response) {
    //variáveis pra receber os parametros do body
    const { name, email, password } = request.body;

    //instancia do servico CreateUsersService
    const createUsers = new CreateUserService();

    //executa o método execute do service CreateUsers
    const users = await createUsers.execute({
      name,
      email,
      password,
    });

    //retorna um json
    return response.json(instanceToInstance(users));
  }

  public async delete(request: Request, response: Response) {
    //variável pra receber os parametros do request
    const { email } = request.params;

    //instancia do servico DeleteUsersService
    const deleteUsers = new DeleteUsersService();

    //executa o método execute do service DeleteUsers
    await deleteUsers.execute({ email });

    //retorna um json
    return response.json([]);
  }
}
