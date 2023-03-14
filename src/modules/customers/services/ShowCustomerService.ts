import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import Customer from '../typeorm/entities/Customer';
import CustomerRepository from '../typeorm/repositories/CustomerRepository';

//interface IRequest
interface IRequest {
  id: string;
}

export default class ShowCustomerService {
  public async execute({ id }: IRequest): Promise<Customer | undefined> {
    //instancia o getCustomRepositury CustomerRepository
    const customerRepository = getCustomRepository(CustomerRepository);

    //executa o método findOne da instância do CustomerRepository
    const customer = await customerRepository.findOne(id);

    //verifica se não existe o Customer
    //se for true libera a excessão AppError
    if (!customer) {
      throw new AppError('Customer no found');
    }

    //retorna um objeto de Customer
    return customer;
  }
}
