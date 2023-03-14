import productsRouter from '@modules/prdocucts/routes/products.routes';
import passwordRouter from '@modules/users/routes/password.routes';
import sessionsRouter from '@modules/users/routes/sessions.routes';
import usersRouter from '@modules/users/routes/users.routes';
import profileRouter from '@modules/users/routes/profile.routes';
import customerRouter from '@modules/customers/routes/customers.routes';
import { Router } from 'express';
import orderRouter from '@modules/orders/routes/orders.routes';

//instancia do Router
const routes = Router();

//rotas products
routes.use('/products', productsRouter);
routes.use('/users', usersRouter);
routes.use('/sessions', sessionsRouter);
routes.use('/password', passwordRouter);
routes.use('/profile', profileRouter);
routes.use('/customers', customerRouter);
routes.use('/orders', orderRouter);

export default routes;
