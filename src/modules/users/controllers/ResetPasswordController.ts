import { Request, Response } from 'express';
import ResetPasswordService from '../services/ResetPasswordService';

export default class ResetPasswordController {
  public async create(request: Request, response: Response) {
    //variáveis pra receber os parametros do body
    const { token, password } = request.body;

    //instancia do servico SendForgotPasswordEmailService
    const resetPasswordEmail = new ResetPasswordService();

    //executa o método execute do service SendForgotPasswordEmailService
    await resetPasswordEmail.execute({
      token,
      password,
    });

    //retorna um json
    return response.status(204).json();
  }
}
