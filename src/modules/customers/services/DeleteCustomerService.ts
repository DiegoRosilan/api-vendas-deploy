import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import CustomerRepository from '../typeorm/repositories/CustomerRepository';

//interface IRequest
interface IRequest {
  id: string;
}

export default class DeleteCustomerService {
  public async execute({ id }: IRequest): Promise<void> {
    //instancia o getCustomRepositury CustomerRepository
    const customerRepository = getCustomRepository(CustomerRepository);

    //executa o método findOne da instância do CustomerRepository
    const customer = await customerRepository.findOne(id);

    //verifica se não existe um produto
    //se for true libera a excessão AppError
    if (!customer) {
      throw new AppError('Customer no found');
    }

    //executa o método remove da instância do CustomerRepository
    await customerRepository.remove(customer);
  }
}
