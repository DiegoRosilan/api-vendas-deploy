import CustomerRepository from '@modules/customers/typeorm/repositories/CustomerRepository';
import ProductRepository from '@modules/prdocucts/typeorm/repositories/ProductsRepository';
import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import Order from '../typeorm/entities/Order';
import OrderRepository from '../typeorm/repositories/OrdersRepository';

interface IProduct {
  id: string;
  quantity: number;
}

interface IRequest {
  customer_id: string;
  products: IProduct[];
}

export default class CreateOrderService {
  public async execute({ customer_id, products }: IRequest): Promise<Order> {
    //instancia o getCustomRepositury OrderRepository
    const orderRepository = getCustomRepository(OrderRepository);
    //instancia o getCustomRepositury CustomerRepository
    const customerRepository = getCustomRepository(CustomerRepository);
    //instancia o getCustomRepositury productRepository
    const productRepository = getCustomRepository(ProductRepository);

    //executa o método findById da instância
    const customerExists = await customerRepository.findById(customer_id);

    //verifica se existe o customer
    if (!customerExists) {
      throw new AppError('Could not find any customer with the give id.');
    }
    //executa o método findById da instância
    const existsProducts = await productRepository.findByIds(products);

    //verifica se existe o product
    if (!existsProducts.length) {
      throw new AppError('Could not find any customer with the give id');
    }
    //executa o método map (mapeia o array) existsProducts e pega os produtos existentes
    const existsProductIds = existsProducts.map(product => product.id);

    //executa o método map (mapeia o array) existsProducts e pega os produtos inexistentes
    const checkInexistProducts = products.filter(
      product => !existsProductIds.includes(product.id),
    );

    //checa quias os produtos existem products
    if (checkInexistProducts.length) {
      throw new AppError(
        `Could not find product ${checkInexistProducts[0].id}`,
      );
    }

    //executa o método para verificar as quantidades do estoque
    const quantityAvaliable = products.filter(
      product =>
        existsProducts.filter(p => p.id === product.id)[0].quantity <
        product.quantity,
    );

    //verifica se os produtos possuem estoque disponivel
    if (quantityAvaliable.length) {
      throw new AppError(
        `The quantity ${quantityAvaliable[0].quantity} is not avaliable for ${quantityAvaliable[0].id}`,
      );
    }

    //serializa os produtos mapeados para salvar no banco de dados
    const serializedProducts = products.map(product => ({
      product_id: product.id,
      quantity: product.quantity,
      price: existsProducts.filter(p => p.id === product.id)[0].price,
    }));

    //cria uam nova order
    const order = await orderRepository.createOrder({
      customer: customerExists,
      products: serializedProducts,
    });

    //instancia o objeto order
    const { order_products } = order;

    //remove do estoque as quantidades mapeadas em order_products
    const UpdateProductQuantity = order_products.map(product => ({
      id: product.product_id,
      quantity:
        existsProducts.filter(p => p.id === product.product_id)[0].quantity -
        product.quantity,
    }));

    //salva os dados no bando
    await productRepository.save(UpdateProductQuantity);

    //retorna uma order
    return order;
  }
}
