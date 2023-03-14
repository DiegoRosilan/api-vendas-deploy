import { instanceToInstance } from 'class-transformer';
import { Request, Response } from 'express';
import CreateSessionsService from '../services/CreateSessionsService';

export default class SessionsController {
  public async create(request: Request, response: Response): Promise<Response> {
    const { email, password } = request.body;
    //instancia do servico CreateSessionService
    const createSession = new CreateSessionsService();

    //executa o método execute do service execute
    const user = await createSession.execute({
      email,
      password,
    });
    return response.json(instanceToInstance(user));
  }
}
