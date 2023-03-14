import AppError from '@shared/errors/AppError';
import { getCustomRepository } from 'typeorm';
import Customer from '../typeorm/entities/Customer';
import CustomerRepository from '../typeorm/repositories/CustomerRepository';

interface IRequest {
  id: string;
  name: string;
  email: string;
}

export default class UpdateCustomerService {
  public async execute({
    id,
    name,
    email,
  }: IRequest): Promise<Customer | undefined> {
    //instancia o getCustomRepositury CustomerRepository
    const customerRepository = getCustomRepository(CustomerRepository);

    //executa o método findOne da instância do CustomerRepository
    const customer = await customerRepository.findOne(id);

    //verifica se não existe um produto
    //se for true libera a excessão AppError
    if (!customer) {
      throw new AppError('Customer no found');
    }

    //executa o método findByName da instância do CustomerRepository
    const customerExist = await customerRepository.findByEmail(email);

    //verifica se já existe um produto e se nome não foi alterado
    //se for true libera a excessão AppError
    if (customerExist && email !== customer.email) {
      throw new AppError('There is already one Customer with this email');
    }

    //seta os valores recebidos aos campos da tabela
    customer.name = name;
    customer.email = email;

    //executa o método save da instância do CustomerRepository
    await customerRepository.save(customer);

    //retorna um objeto de Customer
    return customer;
  }
}
