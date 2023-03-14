import { Request, Response } from 'express';
import SendForgotPasswordEmailService from '../services/SendForgotPasswordEmailService';

export default class ForgotPasswordController {
  public async create(request: Request, response: Response) {
    //variáveis pra receber os parametros do body
    const { email } = request.body;

    //instancia do servico SendForgotPasswordEmailService
    const sendForgotPasswordEmail = new SendForgotPasswordEmailService();

    //executa o método execute do service SendForgotPasswordEmailService
    await sendForgotPasswordEmail.execute({
      email,
    });

    //retorna um json
    return response.status(204).json();
  }
}
