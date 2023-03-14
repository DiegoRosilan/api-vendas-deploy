import { Request, Response } from 'express';
import CreateOrderService from '../services/CreateOrderService';
import ShowOrderService from '../services/ShowOrderService';

export default class OrderController {
  public async show(request: Request, response: Response) {
    //variável pra receber os parametros do request
    const { id } = request.params;
    //instancia do servico ShowProductsService
    const showOrder = new ShowOrderService();

    //executa o método execute do service ShowProducts
    const order = await showOrder.execute({ id });

    //retorna um json
    return response.json(order);
  }

  public async create(request: Request, response: Response) {
    //variáveis pra receber os parametros do body
    const { customer_id, products } = request.body;

    //instancia do servico CreateProductsService
    const createOrder = new CreateOrderService();

    //executa o método execute do service CreateProducts
    const order = await createOrder.execute({
      customer_id,
      products,
    });

    //retorna um json
    return response.json(order);
  }
}
