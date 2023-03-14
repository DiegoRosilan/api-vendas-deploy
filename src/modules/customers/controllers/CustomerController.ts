import { Request, Response } from 'express';
import CreateCustomerService from '../services/CreateCustomerService';
import DeleteCustomerService from '../services/DeleteCustomerService';
import ListCustomerService from '../services/ListCustomerService';
import ShowCustomerService from '../services/ShowCustomerService';
import UpdateCustomerService from '../services/UpdateCustomerService';

export default class CustomersController {
  //método para listagem
  public async index(request: Request, response: Response) {
    //instancia do servico ListCustomersService
    const listCustomers = new ListCustomerService();

    //executa o método execute do service ListCustomers
    const customers = await listCustomers.execute();

    //retorna um json
    return response.json(customers);
  }

  public async show(request: Request, response: Response) {
    //variável pra receber os parametros do request
    const { id } = request.params;
    //instancia do servico ShowCustomersService
    const showCustomer = new ShowCustomerService();

    //executa o método execute do service ShowCustomers
    const customer = await showCustomer.execute({ id });

    //retorna um json
    return response.json(customer);
  }

  public async create(request: Request, response: Response) {
    //variáveis pra receber os parametros do body
    const { name, email } = request.body;

    //instancia do servico CreateCustomersService
    const createCustomer = new CreateCustomerService();

    //executa o método execute do service CreateCustomers
    const customer = await createCustomer.execute({
      name,
      email,
    });

    //retorna um json
    return response.json(customer);
  }

  public async update(request: Request, response: Response) {
    //variáveis pra receber os parametros do body
    const { name, email } = request.body;
    //variável pra receber os parametros do request
    const { id } = request.params;

    //instancia do servico UpdateCustomersService
    const updateCustomer = new UpdateCustomerService();

    //executa o método execute do service UpdateCustomers
    const customer = await updateCustomer.execute({
      id,
      name,
      email,
    });

    //retorna um json
    return response.json(customer);
  }

  public async delete(request: Request, response: Response) {
    //variável pra receber os parametros do request
    const { id } = request.params;

    //instancia do servico DeleteCustomersService
    const deleteCustomer = new DeleteCustomerService();

    //executa o método execute do service DeleteCustomers
    await deleteCustomer.execute({ id });

    //retorna um json
    return response.json([]);
  }
}
