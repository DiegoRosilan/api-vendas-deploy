import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import Product from '../typeorm/entities/Product';
import ProductRepository from '../typeorm/repositories/ProductsRepository';

//interface IRequest
interface IRequest {
  id: string;
}

export default class ShowProductService {
  public async execute({ id }: IRequest): Promise<Product | undefined> {
    //instancia o getCustomRepositury productRepository
    const productsRepository = getCustomRepository(ProductRepository);

    //executa o método findOne da instância do productRepository
    const product = await productsRepository.findOne(id);

    //verifica se não existe o product
    //se for true libera a excessão AppError
    if (!product) {
      throw new AppError('Product no found');
    }

    //retorna um objeto de product
    return product;
  }
}
