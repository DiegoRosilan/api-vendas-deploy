import RedisCache from '@shared/cache/RedisCache';
import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import Product from '../typeorm/entities/Product';
import ProductRepository from '../typeorm/repositories/ProductsRepository';

interface IRequest {
  id: string;
  name: string;
  price: number;
  quantity: number;
}

export default class UpdateProductService {
  public async execute({
    id,
    name,
    price,
    quantity,
  }: IRequest): Promise<Product | undefined> {
    //instancia o getCustomRepositury productRepository
    const productsRepository = getCustomRepository(ProductRepository);

    //executa o método findOne da instância do productRepository
    const product = await productsRepository.findOne(id);

    //verifica se não existe um produto
    //se for true libera a excessão AppError
    if (!product) {
      throw new AppError('Product no found');
    }

    //executa o método findByName da instância do productRepository
    const productExist = await productsRepository.findByName(name);

    //verifica se já existe um produto e se nome não foi alterado
    //se for true libera a excessão AppError
    if (productExist && name !== product.name) {
      throw new AppError('There is already a Product with this name');
    }
    //const redisCache = new RedisCache();

    //LIMPA O CACHE
    await RedisCache.invalidata('api-vendas-PRODUCT_LIST');

    //seta os valores recebidos aos campos da tabela
    product.name = name;
    product.price = price;
    product.quantity = quantity;

    //executa o método save da instância do productRepository
    await productsRepository.save(product);

    //retorna um objeto de product
    return product;
  }
}
