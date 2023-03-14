import AppError from '@shared/errors/AppError';
import { verify } from 'jsonwebtoken';
import { NextFunction, Request, Response } from 'express';
import authConfig from '@config/auth';

interface ITokenPaylod {
  iat: number;
  exp: number;
  sub: string;
}

//função de autenticação
export default function isAutenticated(
  request: Request,
  Response: Response,
  next: NextFunction,
): void {
  //instancia de autorização do cabeçalho da requisição
  const authHeader = request.headers.authorization;

  //se o cabeçalho for vazio
  //retorna uma excessão
  if (!authHeader) {
    throw new AppError('JWT Token is missing.');
  }

  //paga o valor do token  atraves da desestruturação array do JWT
  const [, token] = authHeader.split(' ');

  try {
    //instancia o metrodo verify passando o token e a secret
    //passa o paylod do token e armazena na variavel
    const decodedToken = verify(token, authConfig.jwt.secret);

    //pega o valor sub do token atraves da interface ITokenPaylod
    const { sub } = decodedToken as ITokenPaylod;

    request.user = {
      id: sub,
    };

    return next();
  } catch {
    throw new AppError('Invalid JWT Token');
  }
}
