import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import Product from '../typeorm/entities/Product';
import ProductRepository from '../typeorm/repositories/ProductsRepository';
import RedisCache from '@shared/cache/RedisCache';

//interface IRequest
interface IRequest {
  name: string;
  price: number;
  quantity: number;
}

export default class CreateProductService {
  public async execute({ name, price, quantity }: IRequest): Promise<Product> {
    //instancia o getCustomRepositury productRepository
    const productsRepository = getCustomRepository(ProductRepository);

    //executa o método findByName da instância do productRepository
    const productExist = await productsRepository.findByName(name);

    //verifica se já existe um produto como mesmo nome
    //se for true libera a excessão AppError
    if (productExist) {
      throw new AppError('There is already a Product with this name');
    }

    // const redisCache = new RedisCache();

    //executa o metodo create do respositorio
    const product = productsRepository.create({
      name,
      price,
      quantity,
    });

    //LIMPA O CACHE
    await RedisCache.invalidata('api-vendas-PRODUCT_LIST');

    //salva o produto no banco
    await productsRepository.save(product);

    //retorna um objeto product
    return product;
  }
}
