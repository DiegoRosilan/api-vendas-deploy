import 'reflect-metadata';
import 'dotenv/config';
import express, { NextFunction, Request, Response } from 'express';
import 'express-async-errors';
import cors from 'cors';
import routes from './routes';
import AppError from '@shared/errors/AppError';
import '@shared/typeorm';
import { errors } from 'celebrate';
import upload from '@config/upload';
import { pagination } from 'typeorm-pagination';
import rateLimiter from '@shared/http/middlewares/rateLimiter';

//instancia do express
const app = express();

app.use(pagination);
app.use(cors());
app.use(express.json());

app.use(rateLimiter);
app.use(pagination);

//rota stática para pasta de imagens do avatar do usuário
app.use('/files', express.static(upload.directory));
//rotas
app.use(routes);

app.use(errors());
//middlware para tratamento de erros e excessões
app.use(
  (error: Error, request: Request, response: Response, next: NextFunction) => {
    //se o erro for uma instancia da classe AppError
    if (error instanceof AppError) {
      return response.status(error.statusCode).json({
        status: 'error',
        message: error.message,
      });
    }

    console.log(error);

    //se o erro não for uma instancia da Classe AppError
    return response.status(500).json({
      status: 'error',
      message: 'Internal Server Error',
    });
  },
);

app.listen(3333, () => {
  console.log('Servidor rodando na porta 3333');
});
