import { Router } from 'express';
import OrderController from '../controller/OrderController';
import { celebrate, Joi, Segments } from 'celebrate';
import isAutenticated from '@modules/users/middlewares/isAutenticated';

const orderRouter = Router();
const orderController = new OrderController();

orderRouter.use(isAutenticated);

orderRouter.get(
  '/:id',
  celebrate({
    [Segments.PARAMS]: {
      id: Joi.string().uuid().required(),
    },
  }),
  orderController.show,
);

orderRouter.post(
  '/',
  celebrate({
    [Segments.BODY]: {
      customer_id: Joi.string().uuid().required(),
      products: Joi.required(),
    },
  }),
  orderController.create,
);

export default orderRouter;
