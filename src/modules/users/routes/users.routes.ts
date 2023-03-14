import { celebrate, Joi, Segments } from 'celebrate';
import { Router } from 'express';
import multer from 'multer';
import UsersController from '../controllers/UsersController';
import isAutenticated from '../middlewares/isAutenticated';
import updateConfig from '@config/upload';
import UsersAvatarController from '../controllers/UsersAvatarController';

const usersRouter = Router();
const usersController = new UsersController();
const userAvatarController = new UsersAvatarController();

const upload = multer(updateConfig.multer);

//rota de listar users
usersRouter.get('/', isAutenticated, usersController.index);

//rota de criar novo user
usersRouter.post(
  '/',
  celebrate({
    [Segments.BODY]: {
      name: Joi.string().required(),
      email: Joi.string().email().required(),
      password: Joi.string().required(),
    },
  }),
  usersController.create,
);

//rota para atualizar o avatar
usersRouter.patch(
  '/avatar',
  //verifica se o usuario está autenticado
  isAutenticated,
  //midlle que vai pegar o arquivo usando o nome do campo 'avatar' como referencia
  upload.single('avatar'),
  //metodo de atualização
  userAvatarController.update,
);

//rota de remover user
usersRouter.delete(
  '/:email',
  isAutenticated,
  celebrate({
    [Segments.PARAMS]: {
      email: Joi.string().email().required(),
    },
  }),
  usersController.delete,
);

export default usersRouter;
