import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import Order from '../typeorm/entities/Order';
import OrderRepository from '../typeorm/repositories/OrdersRepository';

interface IRequest {
  id: string;
}

export default class ShowOrderService {
  public async execute({ id }: IRequest): Promise<Order> {
    //instancia o getCustomRepositury productRepository
    const orderRepository = getCustomRepository(OrderRepository);

    //executa o método findById da instância
    const order = await orderRepository.findById(id);

    //verifica se existe a order
    if (!order) {
      throw new AppError('Order not found.');
    }

    //retorna uma order
    return order;
  }
}
